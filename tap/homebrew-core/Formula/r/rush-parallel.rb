class RushParallel < Formula
  desc "Cross-platform command-line tool for executing jobs in parallel"
  homepage "https://github.com/shenwei356/rush"
  url "https://github.com/shenwei356/rush/releases/download/v0.11.0/rush_windows_amd64.exe.tar.gz"
  sha256 "47dd38a1d527c8e00714623351379dfa2f617cf9f8f399586233c5ca29482a45"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/shenwei356/rush/archive/refs/tags/v0.11.0.tar.gz"
  end

  def install
    bin.install "rush.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/rush --version 2>&1")
  end
end
