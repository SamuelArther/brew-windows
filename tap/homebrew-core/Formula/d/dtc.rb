class Dtc < Formula
  desc "Device tree compiler"
  homepage "https://git.kernel.org/pub/scm/utils/dtc/dtc.git"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-dtc-1.8.1-1-any.pkg.tar.zst"
  sha256 "0f3c4df43133b99e040f7d42666e36e50c9aea2f59a494e0aad3b475f3fdd32d"
  license any_of: ["GPL-2.0-or-later", "BSD-2-Clause"]

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://mirrors.edge.kernel.org/pub/software/utils/dtc/"
    regex(/href=.*?dtc[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  resource "mingw-w64-ucrt-x86_64-libyaml" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libyaml-0.2.5-2-any.pkg.tar.zst"
    sha256 "bf4c28a3a92a46db3c809b0789aef731ae9a0c4283db368c7e8624550fed07f3"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/dtc.exe", "dtc"
    msys2_bin "ucrt64/bin/fdtdump.exe", "fdtdump"
    msys2_bin "ucrt64/bin/fdtget.exe", "fdtget"
    msys2_bin "ucrt64/bin/fdtoverlay.exe", "fdtoverlay"
    msys2_bin "ucrt64/bin/fdtput.exe", "fdtput"
  end

  test do
    (testpath/"test.dts").write <<~EOS
      /dts-v1/;
      / {
      };
    EOS
    system bin/"dtc", "test.dts"
  end
end
