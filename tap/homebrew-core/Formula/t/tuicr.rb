class Tuicr < Formula
  desc "Code review TUI with vim keybindings"
  homepage "https://tuicr.dev/"
  url "https://github.com/agavra/tuicr/releases/download/v0.27.0/tuicr-0.27.0-x86_64-pc-windows-msvc.zip"
  sha256 "c1d68e051b2fdbed1cf0c641bcb1b26f63ef7b9b2ce38d23035a2dd8c616819c"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/agavra/tuicr/archive/refs/tags/v0.27.0.tar.gz"
  end

  def install
    bin.install "tuicr.exe"
  end

  test do
    system "git", "init"
    system "git", "config", "user.name", "test"
    system "git", "config", "user.email", "test@example.com"
    (testpath/"test.txt").write("hello world\n")
    system "git", "add", "test.txt"
    system "git", "commit", "-m", "Initial commit"

    assert_equal "[]\n", shell_output("#{bin}/tuicr review list --all")
  end
end
