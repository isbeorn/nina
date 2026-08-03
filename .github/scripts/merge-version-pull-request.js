const EXPECTED_VERSION_FILES = new Set([
    'CommonAssemblyInfo.cs',
    'NINA.Astrometry/NINA.Astrometry.csproj',
    'NINA.Core/NINA.Core.csproj',
    'NINA.CustomControlLibrary/NINA.CustomControlLibrary.csproj',
    'NINA.Equipment/NINA.Equipment.csproj',
    'NINA.Image/NINA.Image.csproj',
    'NINA.MGEN/NINA.MGEN.csproj',
    'NINA.Platesolving/NINA.PlateSolving.csproj',
    'NINA.Plugin/NINA.Plugin.csproj',
    'NINA.Profile/NINA.Profile.csproj',
    'NINA.Sequencer.Generators/NINA.Sequencer.Generators.csproj',
    'NINA.Sequencer/NINA.Sequencer.csproj',
    'NINA.WPF.Base/NINA.WPF.Base.csproj',
    'nikoncswrapper/nikoncswrapper.csproj',
]);

function postfixFor(version) {
    switch (version.split('.')[3][0]) {
        case '1': return '-nightly';
        case '2': return '-beta';
        case '3': return '-rc';
        case '9': return '';
        default: return null;
    }
}

function changedLines(patch, prefix) {
    return (patch || '')
        .split('\n')
        .filter(line => line.startsWith(prefix) && !line.startsWith(`${prefix}${prefix}`))
        .map(line => line.slice(1).trim())
        .filter(Boolean)
        .sort();
}

function sameLines(actual, expected) {
    const sortedExpected = [...expected].sort();
    return actual.length === sortedExpected.length &&
        actual.every((line, index) => line === sortedExpected[index]);
}

function validateVersionFiles(files, version) {
    if (files.length !== EXPECTED_VERSION_FILES.size ||
        files.some(file => !EXPECTED_VERSION_FILES.has(file.filename))) {
        return 'The pull request changes files outside the version allowlist.';
    }

    const versionParts = version.split('.').map(Number);
    const previousVersion = `${versionParts[0]}.${versionParts[1]}.${versionParts[2]}.${versionParts[3] - 1}`;
    const versionPostfix = postfixFor(version);
    const previousPostfix = postfixFor(previousVersion);

    if (versionParts.some(part => !Number.isSafeInteger(part) || part < 0) ||
        versionParts[3] <= 0 || versionPostfix === null || previousPostfix === null) {
        return `Version ${version} is not a valid one-step release increment.`;
    }

    for (const file of files) {
        if (file.status !== 'modified' || !file.patch) {
            return `Version file ${file.filename} is not a reviewable text modification.`;
        }

        const additions = changedLines(file.patch, '+');
        const deletions = changedLines(file.patch, '-');

        if (file.filename === 'CommonAssemblyInfo.cs') {
            const expectedAdditions = [
                `[assembly: AssemblyVersion("${version}")]`,
                `[assembly: AssemblyFileVersion("${version}")]`,
                `[assembly: AssemblyInformationalVersion("${version}${versionPostfix}")]`,
            ];
            const expectedDeletions = [
                `[assembly: AssemblyVersion("${previousVersion}")]`,
                `[assembly: AssemblyFileVersion("${previousVersion}")]`,
                `[assembly: AssemblyInformationalVersion("${previousVersion}${previousPostfix}")]`,
            ];

            if (!sameLines(additions, expectedAdditions) || !sameLines(deletions, expectedDeletions)) {
                return 'CommonAssemblyInfo.cs contains changes other than the expected one-step version increment.';
            }
        } else {
            const expectedAdditions = [`<Version>${version}${versionPostfix}</Version>`];
            const expectedDeletions = [`<Version>${previousVersion}${previousPostfix}</Version>`];

            if (!sameLines(additions, expectedAdditions) || !sameLines(deletions, expectedDeletions)) {
                return `${file.filename} contains changes other than the expected one-step version increment.`;
            }
        }
    }

    return null;
}

async function mergeVersionPullRequest({ github, context, core }) {
    const owner = context.repo.owner;
    const repo = context.repo.repo;
    const eventPull = context.payload.pull_request;
    const pullNumber = eventPull.number;
    const validatedHeadSha = eventPull.head.sha;

    let { data: pull } = await github.rest.pulls.get({
        owner,
        repo,
        pull_number: pullNumber,
    });

    const allowedBase = pull.base.ref === 'develop' ||
        pull.base.ref === 'master' ||
        /^release\/.+$/.test(pull.base.ref);
    const branchMatch = /^automation\/version-bump\/(\d+\.\d+\.\d+\.\d+)-(\d+)$/.exec(pull.head.ref);
    const validVersionPullRequest = pull.state === 'open' &&
        pull.user.login === 'github-actions[bot]' &&
        pull.head.repo?.full_name === `${owner}/${repo}` &&
        pull.head.sha === validatedHeadSha &&
        allowedBase &&
        branchMatch;

    if (!validVersionPullRequest) {
        core.setFailed(`Pull request #${pullNumber} is not the expected open CI-created version pull request.`);
        return;
    }

    const version = branchMatch[1];
    const preparationRunId = Number(branchMatch[2]);
    const { data: preparationRun } = await github.rest.actions.getWorkflowRun({
        owner,
        repo,
        run_id: preparationRunId,
    });

    const validPreparationRun = preparationRun.name === 'Prepare Version Release' &&
        preparationRun.path === '.github/workflows/prepare-version-release.yml' &&
        preparationRun.event === 'workflow_dispatch' &&
        preparationRun.conclusion === 'success' &&
        preparationRun.actor?.login === owner &&
        preparationRun.head_branch === pull.base.ref;

    if (!validPreparationRun) {
        core.setFailed(`Pull request #${pullNumber} does not reference a successful owner-started preparation run.`);
        return;
    }

    const files = await github.paginate(github.rest.pulls.listFiles, {
        owner,
        repo,
        pull_number: pullNumber,
        per_page: 100,
    });
    const fileValidationError = validateVersionFiles(files, version);
    if (fileValidationError) {
        core.setFailed(fileValidationError);
        return;
    }

    for (let attempt = 0; attempt < 5 && pull.mergeable_state === 'unknown'; attempt++) {
        await new Promise(resolve => setTimeout(resolve, 2000));
        ({ data: pull } = await github.rest.pulls.get({
            owner,
            repo,
            pull_number: pullNumber,
        }));
    }

    if (pull.head.sha !== validatedHeadSha) {
        core.setFailed(`Pull request #${pullNumber} changed after its Build and Test run started.`);
        return;
    }

    if (pull.mergeable_state === 'behind') {
        core.setFailed(`Pull request #${pullNumber} is behind ${pull.base.ref}. Use GitHub's Update branch action so the owner-authored update starts a fresh Build and Test run.`);
        return;
    }

    const merge = await github.rest.pulls.merge({
        owner,
        repo,
        pull_number: pullNumber,
        sha: validatedHeadSha,
        merge_method: 'squash',
        commit_title: pull.title,
        commit_message: `Automated version increment from pull request #${pullNumber}.`,
    });

    if (!merge.data.merged || !merge.data.sha) {
        core.setFailed(`GitHub did not merge pull request #${pullNumber}: ${merge.data.message || 'unknown reason'}`);
        return;
    }

    await github.rest.repos.createDispatchEvent({
        owner,
        repo,
        event_type: 'publish-version',
        client_payload: {
            pull_request_number: pullNumber,
            merge_commit_sha: merge.data.sha,
            target_branch: pull.base.ref,
        },
    });

    try {
        await github.rest.git.deleteRef({
            owner,
            repo,
            ref: `heads/${pull.head.ref}`,
        });
    } catch (error) {
        if (error.status !== 404) {
            core.warning(`Merged pull request #${pullNumber}, but could not delete ${pull.head.ref}: ${error.message}`);
        }
    }

    await core.summary
        .addHeading('Version pull request merged')
        .addLink(`#${pullNumber}`, pull.html_url)
        .addRaw(`\n\nPublication dispatched for ${merge.data.sha}.`)
        .write();
}

module.exports = mergeVersionPullRequest;
module.exports.EXPECTED_VERSION_FILES = EXPECTED_VERSION_FILES;
module.exports.postfixFor = postfixFor;
module.exports.validateVersionFiles = validateVersionFiles;
