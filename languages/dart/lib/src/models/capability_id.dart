// ignore_for_file: type=lint
/// auto generated
///  realtime-presence appears only when requested; it is available exactly when realtime-subscribe is and the runtime's realtime projection enables presence.
enum CapabilityId {
    auth('auth'),
    storage('storage'),
    functionsRest('functions-rest'),
    realtimeSubscribe('realtime-subscribe'),
    realtimePresence('realtime-presence');
    const CapabilityId(this.value);
    final String value;
}
