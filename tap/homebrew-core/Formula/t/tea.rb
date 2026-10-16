class Tea < Formula
  desc "Command-line tool to interact with Gitea servers"
  homepage "https://gitea.com/gitea/tea"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tea-0.16.0-1-any.pkg.tar.zst"
  sha256 "5c18f9ceb0f71ad488cb4477eafbe966702f3f37d942442797f89be32a118fc2"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://gitea.com/gitea/tea/archive/v0.16.0.tar.gz"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/tea.exe", "tea"
  end

  test do
    system bin/"tea", "--version"
  end
end
