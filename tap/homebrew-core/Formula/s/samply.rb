class Samply < Formula
  desc "CLI sampling profiler"
  homepage "https://github.com/mstange/samply"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-samply-0.13.1-1-any.pkg.tar.zst"
  sha256 "368bffd9af176bdf19e24c288346f9ded89cfc7c3f4c13b34d6e2161daab3939"
  license any_of: ["Apache-2.0", "MIT"]

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/mstange/samply/archive/refs/tags/samply-v0.13.1.tar.gz"
    regex(/^samply[._-]v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/samply.exe", "samply"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/samply --version")

    test_perf_json = testpath/"test_perf.json"
    test_perf_json.write ""

    output = shell_output("#{bin}/samply import --no-open #{test_perf_json} 2>&1", 1)
    assert_match "Error importing perf.data file", output
  end
end
