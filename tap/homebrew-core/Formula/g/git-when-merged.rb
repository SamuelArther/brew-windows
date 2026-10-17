class GitWhenMerged < Formula
  include Language::Python::Shebang

  desc "Find where a commit was merged in git"
  homepage "https://github.com/mhagger/git-when-merged"
  url "https://github.com/mhagger/git-when-merged/archive/refs/tags/v1.2.1.tar.gz"
  sha256 "46ba5076981862ac2ad0fa0a94b9a5401ef6b5c5b0506c6e306b76e5798e1f58"
  license "GPL-2.0-only"
  head "https://github.com/mhagger/git-when-merged.git", branch: "master"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/git-when-merged-1.2.1"
    sha256 cellar: :any, x86_64_windows: "a2ee7d34e1515fc15e177cdef3f4aa08181674e59454bbad37217e7f0f5ee179"
  end

  uses_from_macos "python"

  def install
    rewrite_shebang detected_python_shebang(use_python_from_path: true), "src/git_when_merged.py"
    bin.install "src/git_when_merged.py" => "git-when-merged"
  end

  test do
    system "git", "init", "--initial-branch", "master"
    system "git", "config", "user.name", "BrewTestBot"
    system "git", "config", "user.email", "BrewTestBot@example.com"
    touch "foo"
    system "git", "add", "foo"
    system "git", "commit", "-m", "foo"
    system "git", "checkout", "-b", "bar"
    touch "bar"
    system "git", "add", "bar"
    system "git", "commit", "-m", "bar"
    system "git", "checkout", "master"
    system "git", "merge", "--no-ff", "bar"
    touch "baz"
    system "git", "add", "baz"
    system "git", "commit", "-m", "baz"
    system bin/"git-when-merged", "bar"
  end
end
