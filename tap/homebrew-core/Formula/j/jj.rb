class Jj < Formula
  desc "Git-compatible distributed version control system"
  homepage "https://github.com/jj-vcs/jj"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-jj-0.46.0-1-any.pkg.tar.zst"
  sha256 "5ce08313d1b37c5c8386a8d2bd6e79a02c264af4151ba14ca8d0ab40e417b00e"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/jj-vcs/jj/archive/refs/tags/v0.46.0.tar.gz"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/jj.exe", "jj"
  end

  test do
    touch testpath/"README.md"
    system bin/"jj", "git", "init"
    system bin/"jj", "describe", "-m", "initial commit"
    assert_match "README.md", shell_output("#{bin}/jj file list")
    assert_match "initial commit", shell_output("#{bin}/jj log")
  end
end
