class Qrtool < Formula
  desc "Utility for encoding or decoding QR code"
  homepage "https://sorairolake.github.io/qrtool/book/index.html"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-qrtool-0.13.2-1-any.pkg.tar.zst"
  sha256 "89e43cd5bf25d70eeccc566691c65d665933da08d60675f723cc8f286080f961"
  license all_of: [
    "CC-BY-4.0",
    any_of: ["Apache-2.0", "MIT"],
  ]

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/sorairolake/qrtool/archive/refs/tags/v0.13.2.tar.gz"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/qrtool.exe", "qrtool"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/qrtool --version 2>&1")
  end
end
