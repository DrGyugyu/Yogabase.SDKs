package io.yogabase.client.models;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;
import java.time.OffsetDateTime;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
import java.util.UUID;
@jakarta.annotation.Generated("com.microsoft.kiota")
public class ClientConfiguration implements Parsable {
    /**
     * The four core rows (auth, storage, functions-rest, realtime-subscribe) in that order, then one row for each requested-only id the client named in X-Yogabase-Accept-Capabilities, in x-yogabase-requested-only order. A client that names nothing receives exactly the four core rows.
     */
    private java.util.List<Capability> capabilities;
    /**
     * The environmentId property
     */
    private UUID environmentId;
    /**
     * The gatewayEndpoint property
     */
    private String gatewayEndpoint;
    /**
     * The issuedAt property
     */
    private OffsetDateTime issuedAt;
    /**
     * The oidcAuthority property
     */
    private String oidcAuthority;
    /**
     * The oidcDiscoveryEndpoint property
     */
    private String oidcDiscoveryEndpoint;
    /**
     * The oidcPublicClientId property
     */
    private String oidcPublicClientId;
    /**
     * Exact OAuth redirect allowlist. Entries are public HTTPS URLs, loopback HTTP URLs, or lowercase reverse-domain private-use native schemes such as com.example.app:/oauth/callback. Userinfo, query strings, and fragments are forbidden.
     */
    private java.util.List<String> oidcRedirectUris;
    /**
     * The oidcScopes property
     */
    private java.util.List<String> oidcScopes;
    /**
     * The projectId property
     */
    private UUID projectId;
    /**
     * Exact HTTPS origin of the separately deployed SignalR realtime gateway.
     */
    private String realtimeEndpoint;
    /**
     * The realtimePath property
     */
    private String realtimePath;
    /**
     * The schema property
     */
    private String schema;
    /**
     * Creates a new instance of the appropriate class based on discriminator value
     * @param parseNode The parse node to use to read the discriminator value and create the object
     * @return a {@link ClientConfiguration}
     */
    @jakarta.annotation.Nonnull
    public static ClientConfiguration createFromDiscriminatorValue(@jakarta.annotation.Nonnull final ParseNode parseNode) {
        Objects.requireNonNull(parseNode);
        return new ClientConfiguration();
    }
    /**
     * Gets the capabilities property value. The four core rows (auth, storage, functions-rest, realtime-subscribe) in that order, then one row for each requested-only id the client named in X-Yogabase-Accept-Capabilities, in x-yogabase-requested-only order. A client that names nothing receives exactly the four core rows.
     * @return a {@link java.util.List<Capability>}
     */
    @jakarta.annotation.Nullable
    public java.util.List<Capability> getCapabilities() {
        return this.capabilities;
    }
    /**
     * Gets the environmentId property value. The environmentId property
     * @return a {@link UUID}
     */
    @jakarta.annotation.Nullable
    public UUID getEnvironmentId() {
        return this.environmentId;
    }
    /**
     * The deserialization information for the current model
     * @return a {@link Map<String, java.util.function.Consumer<ParseNode>>}
     */
    @jakarta.annotation.Nonnull
    public Map<String, java.util.function.Consumer<ParseNode>> getFieldDeserializers() {
        final HashMap<String, java.util.function.Consumer<ParseNode>> deserializerMap = new HashMap<String, java.util.function.Consumer<ParseNode>>(13);
        deserializerMap.put("capabilities", (n) -> { this.setCapabilities(n.getCollectionOfObjectValues(Capability::createFromDiscriminatorValue)); });
        deserializerMap.put("environmentId", (n) -> { this.setEnvironmentId(n.getUUIDValue()); });
        deserializerMap.put("gatewayEndpoint", (n) -> { this.setGatewayEndpoint(n.getStringValue()); });
        deserializerMap.put("issuedAt", (n) -> { this.setIssuedAt(n.getOffsetDateTimeValue()); });
        deserializerMap.put("oidcAuthority", (n) -> { this.setOidcAuthority(n.getStringValue()); });
        deserializerMap.put("oidcDiscoveryEndpoint", (n) -> { this.setOidcDiscoveryEndpoint(n.getStringValue()); });
        deserializerMap.put("oidcPublicClientId", (n) -> { this.setOidcPublicClientId(n.getStringValue()); });
        deserializerMap.put("oidcRedirectUris", (n) -> { this.setOidcRedirectUris(n.getCollectionOfPrimitiveValues(String.class)); });
        deserializerMap.put("oidcScopes", (n) -> { this.setOidcScopes(n.getCollectionOfPrimitiveValues(String.class)); });
        deserializerMap.put("projectId", (n) -> { this.setProjectId(n.getUUIDValue()); });
        deserializerMap.put("realtimeEndpoint", (n) -> { this.setRealtimeEndpoint(n.getStringValue()); });
        deserializerMap.put("realtimePath", (n) -> { this.setRealtimePath(n.getStringValue()); });
        deserializerMap.put("schema", (n) -> { this.setSchema(n.getStringValue()); });
        return deserializerMap;
    }
    /**
     * Gets the gatewayEndpoint property value. The gatewayEndpoint property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getGatewayEndpoint() {
        return this.gatewayEndpoint;
    }
    /**
     * Gets the issuedAt property value. The issuedAt property
     * @return a {@link OffsetDateTime}
     */
    @jakarta.annotation.Nullable
    public OffsetDateTime getIssuedAt() {
        return this.issuedAt;
    }
    /**
     * Gets the oidcAuthority property value. The oidcAuthority property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getOidcAuthority() {
        return this.oidcAuthority;
    }
    /**
     * Gets the oidcDiscoveryEndpoint property value. The oidcDiscoveryEndpoint property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getOidcDiscoveryEndpoint() {
        return this.oidcDiscoveryEndpoint;
    }
    /**
     * Gets the oidcPublicClientId property value. The oidcPublicClientId property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getOidcPublicClientId() {
        return this.oidcPublicClientId;
    }
    /**
     * Gets the oidcRedirectUris property value. Exact OAuth redirect allowlist. Entries are public HTTPS URLs, loopback HTTP URLs, or lowercase reverse-domain private-use native schemes such as com.example.app:/oauth/callback. Userinfo, query strings, and fragments are forbidden.
     * @return a {@link java.util.List<String>}
     */
    @jakarta.annotation.Nullable
    public java.util.List<String> getOidcRedirectUris() {
        return this.oidcRedirectUris;
    }
    /**
     * Gets the oidcScopes property value. The oidcScopes property
     * @return a {@link java.util.List<String>}
     */
    @jakarta.annotation.Nullable
    public java.util.List<String> getOidcScopes() {
        return this.oidcScopes;
    }
    /**
     * Gets the projectId property value. The projectId property
     * @return a {@link UUID}
     */
    @jakarta.annotation.Nullable
    public UUID getProjectId() {
        return this.projectId;
    }
    /**
     * Gets the realtimeEndpoint property value. Exact HTTPS origin of the separately deployed SignalR realtime gateway.
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getRealtimeEndpoint() {
        return this.realtimeEndpoint;
    }
    /**
     * Gets the realtimePath property value. The realtimePath property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getRealtimePath() {
        return this.realtimePath;
    }
    /**
     * Gets the schema property value. The schema property
     * @return a {@link String}
     */
    @jakarta.annotation.Nullable
    public String getSchema() {
        return this.schema;
    }
    /**
     * Serializes information the current object
     * @param writer Serialization writer to use to serialize this model
     */
    public void serialize(@jakarta.annotation.Nonnull final SerializationWriter writer) {
        Objects.requireNonNull(writer);
        writer.writeCollectionOfObjectValues("capabilities", this.getCapabilities());
        writer.writeUUIDValue("environmentId", this.getEnvironmentId());
        writer.writeStringValue("gatewayEndpoint", this.getGatewayEndpoint());
        writer.writeOffsetDateTimeValue("issuedAt", this.getIssuedAt());
        writer.writeStringValue("oidcAuthority", this.getOidcAuthority());
        writer.writeStringValue("oidcDiscoveryEndpoint", this.getOidcDiscoveryEndpoint());
        writer.writeStringValue("oidcPublicClientId", this.getOidcPublicClientId());
        writer.writeCollectionOfPrimitiveValues("oidcRedirectUris", this.getOidcRedirectUris());
        writer.writeCollectionOfPrimitiveValues("oidcScopes", this.getOidcScopes());
        writer.writeUUIDValue("projectId", this.getProjectId());
        writer.writeStringValue("realtimeEndpoint", this.getRealtimeEndpoint());
        writer.writeStringValue("realtimePath", this.getRealtimePath());
        writer.writeStringValue("schema", this.getSchema());
    }
    /**
     * Sets the capabilities property value. The four core rows (auth, storage, functions-rest, realtime-subscribe) in that order, then one row for each requested-only id the client named in X-Yogabase-Accept-Capabilities, in x-yogabase-requested-only order. A client that names nothing receives exactly the four core rows.
     * @param value Value to set for the capabilities property.
     */
    public void setCapabilities(@jakarta.annotation.Nullable final java.util.List<Capability> value) {
        this.capabilities = value;
    }
    /**
     * Sets the environmentId property value. The environmentId property
     * @param value Value to set for the environmentId property.
     */
    public void setEnvironmentId(@jakarta.annotation.Nullable final UUID value) {
        this.environmentId = value;
    }
    /**
     * Sets the gatewayEndpoint property value. The gatewayEndpoint property
     * @param value Value to set for the gatewayEndpoint property.
     */
    public void setGatewayEndpoint(@jakarta.annotation.Nullable final String value) {
        this.gatewayEndpoint = value;
    }
    /**
     * Sets the issuedAt property value. The issuedAt property
     * @param value Value to set for the issuedAt property.
     */
    public void setIssuedAt(@jakarta.annotation.Nullable final OffsetDateTime value) {
        this.issuedAt = value;
    }
    /**
     * Sets the oidcAuthority property value. The oidcAuthority property
     * @param value Value to set for the oidcAuthority property.
     */
    public void setOidcAuthority(@jakarta.annotation.Nullable final String value) {
        this.oidcAuthority = value;
    }
    /**
     * Sets the oidcDiscoveryEndpoint property value. The oidcDiscoveryEndpoint property
     * @param value Value to set for the oidcDiscoveryEndpoint property.
     */
    public void setOidcDiscoveryEndpoint(@jakarta.annotation.Nullable final String value) {
        this.oidcDiscoveryEndpoint = value;
    }
    /**
     * Sets the oidcPublicClientId property value. The oidcPublicClientId property
     * @param value Value to set for the oidcPublicClientId property.
     */
    public void setOidcPublicClientId(@jakarta.annotation.Nullable final String value) {
        this.oidcPublicClientId = value;
    }
    /**
     * Sets the oidcRedirectUris property value. Exact OAuth redirect allowlist. Entries are public HTTPS URLs, loopback HTTP URLs, or lowercase reverse-domain private-use native schemes such as com.example.app:/oauth/callback. Userinfo, query strings, and fragments are forbidden.
     * @param value Value to set for the oidcRedirectUris property.
     */
    public void setOidcRedirectUris(@jakarta.annotation.Nullable final java.util.List<String> value) {
        this.oidcRedirectUris = value;
    }
    /**
     * Sets the oidcScopes property value. The oidcScopes property
     * @param value Value to set for the oidcScopes property.
     */
    public void setOidcScopes(@jakarta.annotation.Nullable final java.util.List<String> value) {
        this.oidcScopes = value;
    }
    /**
     * Sets the projectId property value. The projectId property
     * @param value Value to set for the projectId property.
     */
    public void setProjectId(@jakarta.annotation.Nullable final UUID value) {
        this.projectId = value;
    }
    /**
     * Sets the realtimeEndpoint property value. Exact HTTPS origin of the separately deployed SignalR realtime gateway.
     * @param value Value to set for the realtimeEndpoint property.
     */
    public void setRealtimeEndpoint(@jakarta.annotation.Nullable final String value) {
        this.realtimeEndpoint = value;
    }
    /**
     * Sets the realtimePath property value. The realtimePath property
     * @param value Value to set for the realtimePath property.
     */
    public void setRealtimePath(@jakarta.annotation.Nullable final String value) {
        this.realtimePath = value;
    }
    /**
     * Sets the schema property value. The schema property
     * @param value Value to set for the schema property.
     */
    public void setSchema(@jakarta.annotation.Nullable final String value) {
        this.schema = value;
    }
}
