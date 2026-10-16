class Hookdeck < Formula
  desc "Forward webhook events from Hookdeck to a local server"
  homepage "https://hookdeck.com"
  url "https://github.com/hookdeck/hookdeck-cli/releases/download/v3.1.0/hookdeck_3.1.0_windows_amd64.tar.gz"
  sha256 "755cf55d28685398f0dfde1cc7280c0ca87fd2976cb8efd988d1bec60193fe4b"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/hookdeck/hookdeck-cli/archive/refs/tags/v3.1.0.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    bin.install "hookdeck.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/hookdeck --version")
    assert_match "Provide a project API key", shell_output("#{bin}/hookdeck ci 2>&1", 1)
  end
end
