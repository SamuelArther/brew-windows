class Pax < Formula
  desc "Portable Archive Interchange archive tool"
  homepage "https://mbsd.evolvis.org/pax.htm"
  url "https://repo.msys2.org/msys/x86_64/pax-20240817-1-x86_64.pkg.tar.zst"
  version "20240817"
  sha256 "3fe6e2d42cf4a68e8090a2c5b7528b0f91483949e6d44038b01025105c0509ba"
  license "MirOS"

  # Prebuilt package from MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  livecheck do
    url "https://mbsd.evolvis.org/MirOS/dist/mir/cpio"
    regex(/href=.*?paxmirabilis[._-]v?(\d+(?:\.\d+)*)\.t/i)
  end

  def install
    msys2_install
    msys2_bin "usr/bin/pax.exe", "pax"
  end

  test do
    (testpath/"foo").write "test"
    system bin/"pax", "-f", testpath/"foo.pax", "-w", testpath/"foo"
    rm testpath/"foo"
    system bin/"pax", "-f", testpath/"foo.pax", "-r"
    assert_path_exists testpath/"foo"
  end
end
