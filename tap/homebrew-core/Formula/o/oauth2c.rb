class Oauth2c < Formula
  desc "User-friendly CLI for OAuth2"
  homepage "https://github.com/SecureAuthCorp/oauth2c"
  url "https://github.com/SecureAuthCorp/oauth2c/releases/download/v1.21.1/oauth2c_1.21.1_Windows_x86_64.tar.gz"
  sha256 "1f918f8c980ad48cd974b27ddb27c690d605b0e32e2eb9ce59889cd5a4e6d26b"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/SecureAuthCorp/oauth2c/archive/refs/tags/v1.21.1.tar.gz"
  end

  def install
    bin.install "oauth2c.exe"
  end

  test do
    assert_match "\"access_token\":",
      shell_output("#{bin}/oauth2c https://oauth2c.us.authz.cloudentity.io/oauth2c/demo " \
                   "--client-id cauktionbud6q8ftlqq0 " \
                   "--client-secret HCwQ5uuUWBRHd04ivjX5Kl0Rz8zxMOekeLtqzki0GPc " \
                   "--grant-type client_credentials " \
                   "--auth-method client_secret_basic " \
                   "--scopes introspect_tokens,revoke_tokens")
  end
end
