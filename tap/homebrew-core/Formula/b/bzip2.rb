class Bzip2 < Formula
  desc "Freely available high-quality data compressor"
  homepage "https://sourceware.org/bzip2/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-bzip2-1.0.8-4-any.pkg.tar.zst"
  sha256 "f03a2174034ddd2d96cecd34f617c5f8e2ef86c812b8d2bb3b8875257f2c8bfa"
  license "bzip2-1.0.6"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://sourceware.org/pub/bzip2/"
    regex(/href=.*?bzip2[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/bunzip2.exe", "bunzip2"
    msys2_bin "ucrt64/bin/bzcat.exe", "bzcat"
    msys2_bin "ucrt64/bin/bzip2.exe", "bzip2"
    msys2_bin "ucrt64/bin/bzip2recover.exe", "bzip2recover"
  end

  test do
    testfilepath = testpath + "sample_in.txt"
    zipfilepath = testpath + "sample_in.txt.bz2"

    testfilepath.write "TEST CONTENT"

    system bin/"bzip2", testfilepath
    system bin/"bunzip2", zipfilepath

    assert_equal "TEST CONTENT", testfilepath.read
  end
end
