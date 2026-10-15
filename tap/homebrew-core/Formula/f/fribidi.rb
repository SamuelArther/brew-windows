class Fribidi < Formula
  desc "Implementation of the Unicode BiDi algorithm"
  homepage "https://github.com/fribidi/fribidi"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-fribidi-1.0.17-1-any.pkg.tar.zst"
  sha256 "3581ee03cf83a9bb0e98bf688c4f532a29553500e317532d90746de295b0f8a8"
  license all_of: ["GPL-2.0-or-later", "LGPL-2.1-or-later"]

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/fribidi/fribidi/releases/download/v1.0.17/fribidi-1.0.17.tar.xz"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/fribidi.exe", "fribidi"
  end

  test do
    (testpath/"test.input").write <<~EOS
      a _lsimple _RteST_o th_oat
    EOS

    assert_match "a simple TSet that", shell_output("#{bin}/fribidi --charset=CapRTL --test test.input")
  end
end
