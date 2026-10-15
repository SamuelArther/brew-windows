class Git < Formula
  desc "Distributed revision control system"
  homepage "https://git-scm.com/"
  url "https://github.com/git-for-windows/git/releases/download/v2.56.0.windows.2/MinGit-2.56.0.2-64-bit.zip"
  version "2.56.0"
  sha256 "da35e72aa21c005a5a0d298cfbae110bc1609a815730ea0dde84b01a1b3cd3be"
  license "GPL-2.0-only"

  # Git for Windows tags look like v2.56.0.windows.2; track the Git version part.
  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)\.windows\.\d+$/i)
    strategy :github_latest
  end

  # MinGit, the official minimal Git for Windows distribution; GUI tools are omitted.

  def install
    install_tree
    windows_exe "libexec/cmd/git.exe", "git"
  end

  test do
    system bin/"git", "init"
    %w[haunted house].each { |f| touch testpath/f }
    system bin/"git", "add", "haunted", "house"
    system bin/"git", "config", "user.name", "'A U Thor'"
    system bin/"git", "config", "user.email", "author@example.com"
    system bin/"git", "commit", "-a", "-m", "Initial Commit"
    assert_equal "haunted\nhouse", shell_output("#{bin}/git ls-files").strip
  end
end
