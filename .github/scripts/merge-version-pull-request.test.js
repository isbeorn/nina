const assert = require('node:assert/strict');
const test = require('node:test');

const mergeVersionPullRequest = require('./merge-version-pull-request.js');
const {
    EXPECTED_VERSION_FILES,
    postfixFor,
    validateVersionFiles,
} = mergeVersionPullRequest;

function versionFiles(previousVersion, version) {
    const previousPostfix = postfixFor(previousVersion);
    const versionPostfix = postfixFor(version);

    return [...EXPECTED_VERSION_FILES].map(filename => {
        if (filename === 'CommonAssemblyInfo.cs') {
            return {
                filename,
                status: 'modified',
                patch: [
                    `-[assembly: AssemblyVersion("${previousVersion}")]`,
                    `-[assembly: AssemblyFileVersion("${previousVersion}")]`,
                    `-[assembly: AssemblyInformationalVersion("${previousVersion}${previousPostfix}")]`,
                    `+[assembly: AssemblyVersion("${version}")]`,
                    `+[assembly: AssemblyFileVersion("${version}")]`,
                    `+[assembly: AssemblyInformationalVersion("${version}${versionPostfix}")]`,
                    '+',
                ].join('\n'),
            };
        }

        return {
            filename,
            status: 'modified',
            patch: [
                `-    <Version>${previousVersion}${previousPostfix}</Version>`,
                `+    <Version>${version}${versionPostfix}</Version>`,
            ].join('\n'),
        };
    });
}

function scenario(overrides = {}) {
    const version = overrides.version || '3.3.0.1052';
    const previousVersion = overrides.previousVersion || '3.3.0.1051';
    const runId = 30773797908;
    const headSha = 'head-sha';
    const base = overrides.base || 'develop';
    const pull = {
        number: 339,
        state: 'open',
        title: `Bump version to ${version}`,
        html_url: 'https://example.test/pull/339',
        user: { login: overrides.user || 'github-actions[bot]' },
        head: {
            ref: `automation/version-bump/${version}-${runId}`,
            sha: overrides.currentHeadSha || headSha,
            repo: { full_name: 'isbeorn/nina' },
        },
        base: { ref: base },
        mergeable_state: overrides.mergeableState || 'clean',
    };
    const calls = {
        merge: [],
        dispatch: [],
        deleteRef: [],
        failures: [],
        warnings: [],
    };
    const files = overrides.files || versionFiles(previousVersion, version);
    const preparationRun = {
        name: overrides.preparationName || 'Prepare Version Release',
        path: overrides.preparationPath || '.github/workflows/prepare-version-release.yml',
        event: 'workflow_dispatch',
        conclusion: 'success',
        actor: { login: overrides.preparationActor || 'isbeorn' },
        head_branch: base,
    };
    const github = {
        rest: {
            pulls: {
                get: async () => ({ data: pull }),
                listFiles: async () => ({ data: files }),
                merge: async input => {
                    calls.merge.push(input);
                    return { data: { merged: true, sha: 'merge-sha' } };
                },
            },
            actions: {
                getWorkflowRun: async () => ({ data: preparationRun }),
                createWorkflowDispatch: async input => calls.dispatch.push(input),
            },
            git: {
                deleteRef: async input => calls.deleteRef.push(input),
            },
        },
        paginate: async () => files,
    };
    const summary = {
        addHeading: () => summary,
        addLink: () => summary,
        addRaw: () => summary,
        write: async () => {},
    };
    const core = {
        setFailed: message => calls.failures.push(message),
        warning: message => calls.warnings.push(message),
        summary,
    };
    const context = {
        repo: { owner: 'isbeorn', repo: 'nina' },
        payload: {
            pull_request: {
                number: 339,
                head: { sha: overrides.eventHeadSha || headSha },
            },
        },
    };

    return { github, context, core, calls, files };
}

test('merges and dispatches a valid CI-created version pull request', async () => {
    const setup = scenario();

    await mergeVersionPullRequest(setup);

    assert.deepEqual(setup.calls.failures, []);
    assert.equal(setup.calls.merge.length, 1);
    assert.equal(setup.calls.merge[0].sha, 'head-sha');
    assert.equal(setup.calls.dispatch.length, 1);
    assert.equal(setup.calls.dispatch[0].workflow_id, 'build-and-release.yml');
    assert.equal(setup.calls.deleteRef.length, 1);
});

test('accepts each protected release target branch', async () => {
    for (const base of ['develop', 'master', 'release/3.3']) {
        const setup = scenario({ base });

        await mergeVersionPullRequest(setup);

        assert.deepEqual(setup.calls.failures, [], `Expected ${base} to be accepted.`);
        assert.equal(setup.calls.merge.length, 1, `Expected ${base} to be merged.`);
    }
});

test('rejects a version pull request targeting any other branch', async () => {
    const setup = scenario({ base: 'feature/unprotected' });

    await mergeVersionPullRequest(setup);

    assert.match(setup.calls.failures[0], /not the expected open CI-created version pull request/);
    assert.equal(setup.calls.merge.length, 0);
});

test('rejects an ordinary contributor pull request', async () => {
    const setup = scenario({ user: 'contributor' });

    await mergeVersionPullRequest(setup);

    assert.match(setup.calls.failures[0], /not the expected open CI-created version pull request/);
    assert.equal(setup.calls.merge.length, 0);
});

test('rejects a preparation run not started by the repository owner', async () => {
    const setup = scenario({ preparationActor: 'maintainer' });

    await mergeVersionPullRequest(setup);

    assert.match(setup.calls.failures[0], /owner-started preparation run/);
    assert.equal(setup.calls.merge.length, 0);
});

test('rejects a pull request with an extra file', async () => {
    const setup = scenario();
    setup.files.push({ filename: 'unrelated.cs', status: 'modified', patch: '-old\n+new' });

    await mergeVersionPullRequest(setup);

    assert.match(setup.calls.failures[0], /outside the version allowlist/);
    assert.equal(setup.calls.merge.length, 0);
});

test('rejects a head commit that changed after CI started', async () => {
    const setup = scenario({ currentHeadSha: 'new-head-sha' });

    await mergeVersionPullRequest(setup);

    assert.match(setup.calls.failures[0], /not the expected open CI-created version pull request/);
    assert.equal(setup.calls.merge.length, 0);
});

test('does not merge a version pull request that is behind its base', async () => {
    const setup = scenario({ mergeableState: 'behind' });

    await mergeVersionPullRequest(setup);

    assert.match(setup.calls.failures[0], /is behind develop/);
    assert.equal(setup.calls.merge.length, 0);
});

test('rejects unintended channel transitions and build overflow', () => {
    for (const [before, after] of [['3.2.0.1999', '3.2.0.2000'], ['3.2.0.2999', '3.2.0.3000'], ['3.2.0.9999', '3.2.0.10000']]) {
        assert.notEqual(validateVersionFiles(versionFiles(before, after), after), null);
    }
});

test('accepts a patch release and rejects a patch decrement or skipped patch', () => {
    assert.equal(validateVersionFiles(versionFiles('3.2.1.9002', '3.2.2.9001'), '3.2.2.9001'), null);
    assert.notEqual(validateVersionFiles(versionFiles('3.2.2.9001', '3.2.1.9001'), '3.2.1.9001'), null);
    assert.notEqual(validateVersionFiles(versionFiles('3.2.1.9001', '3.2.3.9001'), '3.2.3.9001'), null);
});

test('dispatches publication on the exact release branch', async () => {
    const setup = scenario({ base: 'release/3.2.x', previousVersion: '3.2.1.9001', version: '3.2.1.9002' });
    await mergeVersionPullRequest(setup);
    assert.deepEqual(setup.calls.failures, []);
    assert.equal(setup.calls.dispatch[0].ref, 'release/3.2.x');
    assert.equal(setup.calls.dispatch[0].workflow_id, 'build-and-release.yml');
    assert.equal(setup.calls.dispatch[0].inputs.merge_commit_sha, 'merge-sha');
});

test('rejects a 3.3 version on the 3.2 release branch', async () => {
    const setup = scenario({ base: 'release/3.2.x' });
    await mergeVersionPullRequest(setup);
    assert.equal(setup.calls.merge.length, 0);
});

test('rejects increments larger than one', () => {
    const files = versionFiles('3.3.0.1050', '3.3.0.1052');
    assert.match(validateVersionFiles(files, '3.3.0.1052'), /one-step version increment/);
});

test('rejects a version decrement', () => {
    const files = versionFiles('3.3.0.1053', '3.3.0.1052');
    assert.match(validateVersionFiles(files, '3.3.0.1052'), /one-step version increment/);
});

test('accepts preparation through the existing release workflow entry point', async () => {
    const setup = scenario({ preparationName: 'Build and Release Version', preparationPath: '.github/workflows/build-and-release.yml' });
    await mergeVersionPullRequest(setup);
    assert.deepEqual(setup.calls.failures, []);
    assert.equal(setup.calls.dispatch[0].inputs.operation, 'publish');
});

test('rejects duplicate version files', () => {
    const files = versionFiles('3.2.1.9001', '3.2.1.9002');
    files[files.length - 1] = files[0];
    assert.notEqual(validateVersionFiles(files, '3.2.1.9002'), null);
});
