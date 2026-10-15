class Glew < Formula
  desc "OpenGL Extension Wrangler Library"
  homepage "https://glew.sourceforge.net/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-glew-2.3.1-4-any.pkg.tar.zst"
  sha256 "69cf8d2d95edc36f481d44e2a926520bd43d3aa098246e46b57abe5d11364065"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://downloads.sourceforge.net/project/glew/glew/2.3.1/glew-2.3.1.tgz"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/glewinfo.exe", "glewinfo"
    msys2_bin "ucrt64/bin/visualinfo.exe", "visualinfo"
  end

  test do
    assert_path_exists bin/"glewinfo.exe"
  end
end
