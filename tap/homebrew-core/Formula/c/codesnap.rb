class Codesnap < Formula
  desc "Generates code snapshots in various formats"
  homepage "https://codesnap-docs.netlify.app/"
  url "https://github.com/codesnap-rs/codesnap/releases/download/v0.14.0/codesnap-cli_v0.14.0_x86_64-pc-windows-msvc.zip"
  sha256 "bc0f031c96cfc7512e1f56f7db31c36fa14461816445b69e43b914b3302fd8c7"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/codesnap-rs/codesnap/archive/refs/tags/v0.14.0.tar.gz"
  end

  def install
    bin.install "codesnap.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/codesnap --version 2>&1")
  end
end
