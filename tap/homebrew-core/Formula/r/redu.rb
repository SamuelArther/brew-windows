class Redu < Formula
  desc "Ncdu for your restic repository"
  homepage "https://github.com/drdo/redu"
  url "https://github.com/drdo/redu/releases/download/v0.2.15/redu-0.2.15-windows-x86_64.zip"
  sha256 "cd28552e8c6d0a422b2e04095b3c565e9c548f8fe0da8d3d4129bce3575508bb"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/drdo/redu/archive/refs/tags/v0.2.15.tar.gz"
  end

  def install
    bin.install "redu.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/redu --version")
    assert_match "Error: restic error", shell_output("#{bin}/redu --repo mock_repo mock_pw 2>&1", 1)
  end
end
