class Premake < Formula
  desc "Write once, build anywhere Lua-based build system"
  homepage "https://premake.github.io/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-premake-5.0beta6-2-any.pkg.tar.zst"
  sha256 "c8a455f2fbf175f13e336010cfde6089db27c96738f2b3bb251a66cff30a6cdb"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://premake.github.io/download/"
    regex(/href=.*?premake[._-]v?(\d+(?:\.\d+)+(?:[._-][a-z]+\d*)?)[._-]src\.zip/i)
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/premake5.exe", "premake5"
  end

  test do
    system bin/"premake5", "--version"
  end
end
