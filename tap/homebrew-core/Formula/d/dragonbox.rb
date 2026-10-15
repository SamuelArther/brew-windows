class Dragonbox < Formula
  desc "Reference implementation of Dragonbox in C++"
  homepage "https://github.com/jk-jeon/dragonbox"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-dragonbox-1.1.3-1-any.pkg.tar.zst"
  sha256 "ede116b7994ab5973c7aec564b4af145921638785550a5e61bf7e111a5fb3eb8"
  license any_of: [
    "BSL-1.0",
    "Apache-2.0" => { with: "LLVM-exception" },
  ]

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/jk-jeon/dragonbox/archive/refs/tags/1.1.3.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"dragonbox-1.1.3/dragonbox/dragonbox.h"
  end
end
