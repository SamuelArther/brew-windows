class GitSpice < Formula
  desc "Manage stacked Git branches"
  homepage "https://abhinav.github.io/git-spice/"
  url "https://github.com/abhinav/git-spice/releases/download/v0.32.0/git-spice.Windows-x86_64.tar.gz"
  sha256 "a8de09b2451fbc6ae598c801a29507f9178edb8d428b3620fa689f3946b99ea0"
  license "GPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/abhinav/git-spice/archive/refs/tags/v0.32.0.tar.gz"
    strategy :github_latest
  end

  def install
    bin.install "git-spice.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/git-spice --version 2>&1")
  end
end
