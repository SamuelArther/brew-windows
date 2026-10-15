class Podofo < Formula
  desc "Library to work with the PDF file format"
  homepage "https://github.com/podofo/podofo"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-podofo-1.1.1-2-any.pkg.tar.zst"
  sha256 "9247708f5368dd8afbf2f534dba4e24604aca5cd71dcf87324c3030fe9ab779f"
  license all_of: [
    { any_of: ["LGPL-2.0-or-later", "MPL-2.0"] },
    "GPL-2.0-or-later", # tools/

    # Additional licenses used in specific files
    "Apache-2.0", # src/podofo/private/FontUtils*
    "MIT",        # src/podofo/private/SASLprep*
    any_of: [     # src/podofo/private/OpenSSLInternal*
      { "LGPL-2.0-or-later" => { with: "cryptsetup-OpenSSL-exception" } },
      "MPL-2.0",
    ],
  ]

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/podofo/podofo/archive/refs/tags/1.1.2.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"podofo/podofo.h"
    assert_path_exists lib/"libpodofo.dll.a"
    assert_path_exists bin/"libpodofo.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libpodofo.pc").read
  end
end
