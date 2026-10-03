// ignore_for_file: type=lint
/// auto generated
///  What a schedule asks for when an occurrence is due while the one before it is still running: Allow starts it alongside, Forbid skips it, Replace stops the running one and starts it. The value is checked and stored with the schedule, but the scheduler does not yet look for a running occurrence, so every occurrence starts whichever is chosen. An unknown name is a bare 400. Always send it: the gateway reads a missing one as Allow, the first name, which this contract does not promise.
enum ConcurrencyPolicy {
    allow('Allow'),
    forbid('Forbid'),
    replace('Replace');
    const ConcurrencyPolicy(this.value);
    final String value;
}
