class FoxgloveCli < Formula
  desc "Foxglove command-line tool"
  homepage "https://github.com/foxglove/foxglove-cli"
  url "https://github.com/foxglove/foxglove-cli/releases/download/v2.0.0/foxglove-windows-amd64.exe"
  sha256 "bc4cfc5317bdd2a5e818417011fc58b60722434fa73169dd132c3411c84f8343"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/foxglove/foxglove-cli/archive/refs/tags/v2.0.0.tar.gz"
  end

  def install
    bin.install "foxglove-windows-amd64.exe" => "foxglove.exe"
  end

  test do
    system bin/"foxglove", "auth", "configure-api-key", "--api-key", "foobar"
    expected = "Authenticated with API key"
    assert_match expected, shell_output("#{bin}/foxglove auth info")
    assert_match version.to_s, shell_output("#{bin}/foxglove version")
  end
end
