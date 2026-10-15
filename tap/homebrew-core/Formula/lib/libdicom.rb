class Libdicom < Formula
  desc "DICOM WSI read library"
  homepage "https://github.com/ImagingDataCommons/libdicom"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libdicom-1.3.0-1-any.pkg.tar.zst"
  sha256 "f8388114f710953cebf3ebc35a810520948aa4e30035c17cd8e3848c9e3c15e0"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/ImagingDataCommons/libdicom/releases/download/v1.3.0/libdicom-1.3.0.tar.xz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/dcm-dump.exe", "dcm-dump"
    msys2_bin "ucrt64/bin/dcm-getframe.exe", "dcm-getframe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/dcm-dump -V 2>&1")
  end
end
