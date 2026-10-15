class Verilator < Formula
  desc "Verilog simulator"
  homepage "https://www.veripool.org/wiki/verilator"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-verilator-5.050-1-any.pkg.tar.zst"
  sha256 "a3adb510e9a5298c0ff563af570a4411ea11fd19f7ecb12c8780c561ff462c86"
  license any_of: ["LGPL-3.0-only", "Artistic-2.0"]

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/verilator/verilator/archive/refs/tags/v5.052.tar.gz"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/verilator_bin.exe", "verilator_bin"
    msys2_bin "ucrt64/bin/verilator_bin_dbg.exe", "verilator_bin_dbg"
    msys2_bin "ucrt64/bin/verilator_coverage_bin_dbg.exe", "verilator_coverage_bin_dbg"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/verilator_bin --version 2>&1")
  end
end
