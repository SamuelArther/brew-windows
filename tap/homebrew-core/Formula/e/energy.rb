class Energy < Formula
  desc "CLI is used to initialize the Energy development environment tools"
  homepage "https://energye.github.io"
  url "https://github.com/energye/energy/releases/download/v2.5.6/energy-windows32.zip"
  sha256 "20bee6dd3d6ab9ededa00fe9a57e114a0e1ceb025c70fff5f62f5ea7c5beef07"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/energye/energy/archive/refs/tags/v2.5.6.tar.gz"
    strategy :github_latest
  end

  def install
    bin.install "energy-windows32.exe" => "energy.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/energy --version 2>&1")
  end
end
