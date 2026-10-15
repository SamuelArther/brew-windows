class GitWho < Formula
  desc "Git blame for file trees"
  homepage "https://github.com/sinclairtarget/git-who"
  url "https://github.com/sinclairtarget/git-who/releases/download/v1.3/gitwho_v1.3_windows_amd64.tar.gz"
  sha256 "9bdc00a52e6307332a909a0b9bd46354442dd9c83a34c4741f2a866833852cec"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/sinclairtarget/git-who/archive/refs/tags/v1.3.tar.gz"
    strategy :github_latest
  end

  def install
    bin.install "git-who.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/git-who -version")

    system "git", "init"
    touch "example"
    system "git", "add", "example"
    system "git", "commit", "-m", "example"

    assert_match "example", shell_output("#{bin}/git-who tree")
  end
end
