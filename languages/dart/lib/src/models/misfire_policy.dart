// ignore_for_file: type=lint
/// auto generated
///  What the scheduler does about occurrences it missed while it was unavailable. RunImmediately runs once, now, however many were missed, and resumes the cadence. SkipToNextOccurrence abandons what it missed and resumes at the next occurrence. RunAllMissed runs each missed occurrence, oldest first, up to maxCatchUp. An unknown name is a bare 400. Always send it: the gateway reads a missing one as RunImmediately, the first name, which this contract does not promise.
enum MisfirePolicy {
    runImmediately('RunImmediately'),
    skipToNextOccurrence('SkipToNextOccurrence'),
    runAllMissed('RunAllMissed');
    const MisfirePolicy(this.value);
    final String value;
}
