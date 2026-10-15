class Rustscan < Formula
  desc "Modern Day Portscanner"
  homepage "https://github.com/bee-san/RustScan"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-rustscan-2.4.1-1-any.pkg.tar.zst"
  sha256 "2b1017e94cf197901060f3af0eed8778eb4fee379ef3df99e8999796d03301ea"
  license "GPL-3.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/bee-san/RustScan/archive/refs/tags/2.4.1.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/rustscan.exe", "rustscan"
  end

  test do
    refute_match("panic", shell_output("#{bin}/rustscan --greppable -a 127.0.0.1"))
    refute_match("panic", shell_output("#{bin}/rustscan --greppable -a 0.0.0.0"))
  end
end
