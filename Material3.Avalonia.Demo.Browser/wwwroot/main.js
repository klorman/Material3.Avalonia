import { dotnet } from './_framework/dotnet.js';
try {
    const { runMain } = await dotnet.withDiagnosticTracing(false).withExitOnUnhandledError(false).create();
    await runMain();
} catch (error) {
    console.error(error);
    document.getElementById('out').textContent = `The demo could not start: ${error}`;
}
