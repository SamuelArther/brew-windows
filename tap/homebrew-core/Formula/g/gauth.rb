class Gauth < Formula
  desc "Google Authenticator in your terminal"
  homepage "https://github.com/pcarrier/gauth"
  url "https://github.com/pcarrier/gauth/releases/download/v1.5.0/gauth_1.5.0_windows_amd64.exe"
  sha256 "38f56e2fecd275aff05584afa409bf95db543d10a31adf16903f83d957c9326a"
  license "ISC"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/pcarrier/gauth/archive/refs/tags/v1.5.0.tar.gz"
  end

  def install
    bin.install "gauth_1.5.0_windows_amd64.exe" => "gauth.exe"
  end

  test do
    refute_empty pipe_output "#{bin}/gauth demo --add", "JBSWY3DPEHPK3PXP"
    assert_match(/demo(\s+\d{6}){3}/, shell_output(bin/"gauth"))
  end
end
