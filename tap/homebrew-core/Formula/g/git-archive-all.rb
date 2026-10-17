class GitArchiveAll < Formula
  include Language::Python::Shebang

  desc "Archive a project and its submodules"
  homepage "https://github.com/Kentzo/git-archive-all"
  url "https://github.com/Kentzo/git-archive-all/archive/refs/tags/1.23.1.tar.gz"
  sha256 "0c440d15be18e336672549231510fa3c66d0cb95069a5e4800fdd876ab6814df"
  license "MIT"
  head "https://github.com/Kentzo/git-archive-all.git", branch: "master"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/git-archive-all-1.23.1"
    sha256 cellar: :any, x86_64_windows: "cb267c064462d9db4ec5257d539cdb8b0c7ff7412f0a2290dd6e792420f4fd5b"
  end

  uses_from_macos "python"

  def install
    rewrite_shebang detected_python_shebang(use_python_from_path: true), "git_archive_all.py"

    system "make", "prefix=#{prefix}", "install"
  end

  test do
    system "git", "init"
    touch "homebrew"
    system "git", "add", "homebrew"
    system "git", "commit", "--message", "brewing"

    assert_equal "#{testpath.realpath}/homebrew => archive/homebrew",
                 shell_output("#{bin}/git-archive-all --dry-run ./archive").chomp
  end
end
