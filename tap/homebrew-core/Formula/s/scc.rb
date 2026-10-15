class Scc < Formula
  desc "Fast and accurate code counter with complexity and COCOMO estimates"
  homepage "https://github.com/boyter/scc/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-scc-4.1.0-1-any.pkg.tar.zst"
  sha256 "43f460cf242d3878596fe807727bbdecb3e034c76c92efc77dae9ba1b2f30746"
  license any_of: ["MIT", "Unlicense"]

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url :homepage
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/scc.exe", "scc"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/scc --version")

    (testpath/"test.c").write <<~C
      #include <stdio.h>
      int main(void) {
        return 0;
      }
    C

    expected_output = <<~CSV
      Language,Lines,Code,Comments,Blanks,Complexity,Bytes,Files,ULOC
      C,4,4,0,0,0,50,1,0
    CSV

    assert_match expected_output, shell_output("#{bin}/scc -fcsv test.c")
  end
end
