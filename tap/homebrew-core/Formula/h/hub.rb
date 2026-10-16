class Hub < Formula
  desc "Add GitHub support to git on the command-line"
  homepage "https://hub.github.com/"
  url "https://github.com/mislav/hub/releases/download/v2.14.2/hub-windows-amd64-2.14.2.zip"
  sha256 "2ad771ff5e5e47c8d48aa22f6852ebef87e1fe0f53a1194ddcbe615a9ddbc59e"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/mislav/hub/archive/refs/tags/v2.14.2.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/bin/hub.exe", "hub"
  end

  test do
    system "git", "init"
    %w[haunted house].each { |f| touch testpath/f }
    system "git", "add", "haunted", "house"
    system "git", "commit", "-a", "-m", "Initial Commit"
    assert_equal "haunted\nhouse", shell_output("#{bin}/hub ls-files").strip
  end
end
