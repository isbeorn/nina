const assert = require('node:assert/strict');
const test = require('node:test');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { EXPECTED_VERSION_FILES, nextVersion, postfixFor, validateBranchVersion, updateVersionFiles } = require('./release-version');

for (const [build, suffix] of [[1000, '-nightly'], [2000, '-beta'], [3000, '-rc'], [9000, '']]) {
    test(`build and patch increments retain channel ${build}`, () => {
        assert.equal(nextVersion(`3.2.0.${build}`), `3.2.0.${build + 1}`);
        assert.equal(nextVersion(`3.2.0.${build + 999}`, 'patch'), `3.2.1.${build + 1}`);
        assert.equal(postfixFor(`3.2.0.${build}`), suffix);
        assert.equal(postfixFor(`3.2.0.${build + 999}`), suffix);
        assert.throws(() => nextVersion(`3.2.0.${build + 999}`), /boundary/);
    });
}

test('rejects underflow, overflow, malformed versions and channel rollover', () => {
    for (const version of ['3.2.-1.9001', '3.2.65535.9001', '3.2.0.999', '3.2.0.10000', '03.2.0.9001', '3.2.0.4000']) {
        assert.throws(() => nextVersion(version));
        assert.equal(postfixFor(version), null);
    }
    assert.throws(() => nextVersion('3.2.65534.9999', 'patch'));
    assert.equal(nextVersion('3.2.65533.9999', 'patch'), '3.2.65534.9001');
    assert.throws(() => nextVersion('3.2.0.9001', 'decrement'));
});

test('release branch fixes major/minor while develop remains supported', () => {
    for (const branch of ['develop', 'master', 'release/3.2.x', 'release/3.2']) validateBranchVersion(branch, '3.2.1.9001');
    for (const branch of ['feature/test', 'release/3.3.x', 'release/3.1.x', 'release/foo']) {
        assert.throws(() => validateBranchVersion(branch, '3.2.1.9001'));
    }
});

function fixture(version) {
    const root = fs.mkdtempSync(path.join(os.tmpdir(), 'nina-version-test-'));
    const suffix = postfixFor(version);
    for (const file of EXPECTED_VERSION_FILES) {
        const content = file === 'CommonAssemblyInfo.cs'
            ? `\ufeff// retained header\r\n[assembly: AssemblyVersion("${version}")]\r\n[assembly: AssemblyFileVersion("${version}")]\r\n[assembly: AssemblyInformationalVersion("${version}${suffix}")]\r\n`
            : `<Project>\r\n  <PropertyGroup>\r\n    <Version>${version}${suffix}</Version>\r\n  </PropertyGroup>\r\n</Project>\r\n`;
        fs.mkdirSync(path.dirname(path.join(root, file)), { recursive: true });
        fs.writeFileSync(path.join(root, file), content);
    }
    return root;
}

for (const increment of ['build', 'patch']) {
    test(`updates only version text for ${increment} releases`, () => {
        const root = fixture('3.2.1.9001');
        try {
            const before = new Map([...EXPECTED_VERSION_FILES].map(file => [file, fs.readFileSync(path.join(root, file), 'utf8')]));
            const result = updateVersionFiles(root, increment, 'release/3.2.x');
            const expected = increment === 'build' ? '3.2.1.9002' : '3.2.2.9001';
            assert.equal(result.version, expected);
            for (const [file, content] of before) {
                assert.equal(fs.readFileSync(path.join(root, file), 'utf8'), content.replaceAll('3.2.1.9001', expected));
            }
        } finally { fs.rmSync(root, { recursive: true }); }
    });
}

test('inconsistent project leaves all version files untouched', () => {
    const root = fixture('3.2.1.9001');
    try {
        fs.writeFileSync(path.join(root, 'nikoncswrapper/nikoncswrapper.csproj'), '<Project/>');
        const before = fs.readFileSync(path.join(root, 'CommonAssemblyInfo.cs'), 'utf8');
        assert.throws(() => updateVersionFiles(root, 'patch', 'release/3.2.x'), /Inconsistent/);
        assert.equal(fs.readFileSync(path.join(root, 'CommonAssemblyInfo.cs'), 'utf8'), before);
    } finally { fs.rmSync(root, { recursive: true }); }
});
