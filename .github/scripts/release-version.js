const fs = require('node:fs');
const path = require('node:path');

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
    'NINA.Sequencer/NINA.Sequencer.csproj',
    'NINA.WPF.Base/NINA.WPF.Base.csproj',
    'nikoncswrapper/nikoncswrapper.csproj',
]);

function parseVersion(version) {
    if (!/^\d+\.\d+\.\d+\.\d+$/.test(version)) throw new Error('Invalid four-part version.');
    const parts = version.split('.').map(Number);
    if (parts.some(part => !Number.isInteger(part) || part < 0 || part > 65534) || parts.join('.') !== version) {
        throw new Error('Version components must be canonical integers between 0 and 65534.');
    }
    const channel = Math.floor(parts[3] / 1000);
    if (![1, 2, 3, 9].includes(channel)) throw new Error('Build must remain in a supported four-digit release channel.');
    return parts;
}

function postfixFor(version) {
    try { return ({ 1: '-nightly', 2: '-beta', 3: '-rc', 9: '' })[Math.floor(parseVersion(version)[3] / 1000)]; }
    catch { return null; }
}

function validateBranchVersion(branch, version) {
    const parts = parseVersion(version);
    if (branch === 'develop' || branch === 'master') return;
    const match = /^release\/(\d+)\.(\d+)(?:\.x)?$/.exec(branch);
    if (!match || Number(match[1]) !== parts[0] || Number(match[2]) !== parts[1]) {
        throw new Error('The version must match its release branch major and minor numbers.');
    }
}

function nextVersion(version, increment = 'build') {
    const parts = parseVersion(version);
    const channel = Math.floor(parts[3] / 1000);
    if (increment === 'build') {
        parts[3]++;
        if (Math.floor(parts[3] / 1000) !== channel) throw new Error('Build increment would cross a release channel boundary.');
    } else if (increment === 'patch') {
        parts[2]++;
        parts[3] = channel * 1000 + 1;
    } else {
        throw new Error('Increment must be build or patch.');
    }
    const result = parts.join('.');
    parseVersion(result);
    return result;
}

function updateVersionFiles(root, increment, branch) {
    const assemblyPath = path.join(root, 'CommonAssemblyInfo.cs');
    const source = fs.readFileSync(assemblyPath, 'utf8');
    const match = /\[assembly: AssemblyVersion\("([^"]+)"\)\]/.exec(source);
    if (!match) throw new Error('AssemblyVersion is missing.');
    const previous = match[1];
    validateBranchVersion(branch, previous);
    const version = nextVersion(previous, increment);
    validateBranchVersion(branch, version);
    const oldPostfix = postfixFor(previous);
    const postfix = postfixFor(version);
    const edits = [];
    for (const file of EXPECTED_VERSION_FILES) {
        const filename = path.join(root, file);
        let content = fs.readFileSync(filename, 'utf8');
        const pairs = file === 'CommonAssemblyInfo.cs'
            ? ['AssemblyVersion', 'AssemblyFileVersion', 'AssemblyInformationalVersion'].map(attribute => [
                `[assembly: ${attribute}("${previous}${attribute === 'AssemblyInformationalVersion' ? oldPostfix : ''}")]`,
                `[assembly: ${attribute}("${version}${attribute === 'AssemblyInformationalVersion' ? postfix : ''}")]`,
            ])
            : [[`<Version>${previous}${oldPostfix}</Version>`, `<Version>${version}${postfix}</Version>`]];
        for (const [before, after] of pairs) {
            if (content.split(before).length !== 2) throw new Error(`Inconsistent or duplicate version in ${file}.`);
            content = content.replace(before, after);
        }
        edits.push([filename, content]);
    }
    // Validate every file before writing any of them; retain BOMs, whitespace and line endings.
    for (const [filename, content] of edits) fs.writeFileSync(filename, content, 'utf8');
    return { version, assembly_postfix: postfix };
}

module.exports = { EXPECTED_VERSION_FILES, parseVersion, postfixFor, validateBranchVersion, nextVersion, updateVersionFiles };

if (require.main === module) {
    try {
        const args = process.argv.slice(2);
        const option = name => args[args.indexOf(name) + 1];
        if (args.includes('--validate')) {
            validateBranchVersion(option('--branch'), option('--validate'));
        } else {
            const result = updateVersionFiles(process.cwd(), option('--increment'), option('--branch'));
            for (const [key, value] of Object.entries(result)) {
                console.log(`${key}=${value}`);
                if (process.env.GITHUB_OUTPUT) fs.appendFileSync(process.env.GITHUB_OUTPUT, `${key}=${value}\n`);
            }
        }
    } catch (error) {
        console.error(error.message);
        process.exitCode = 1;
    }
}
