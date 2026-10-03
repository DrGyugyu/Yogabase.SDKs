// ignore_for_file: type=lint
/// auto generated
///  What the scheduler does about occurrences it missed while it was unavailable. RunImmediately runs once, now, however many were missed, and resumes the cadence. SkipToNextOccurrence abandons what it missed and resumes at the next occurrence. RunAllMissed runs each missed occurrence, oldest first, up to maxCatchUp. An unknown name is a bare 400. Required: a request that leaves it out is a 400 whose problem names the member.
enum MisfirePolicy {
    runImmediately('RunImmediately'),
    skipToNextOccurrence('SkipToNextOccurrence'),
    runAllMissed('RunAllMissed');
    const MisfirePolicy(this.value);
    final String value;
}
