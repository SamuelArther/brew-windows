class RustParallel < Formula
  desc "Run commands in parallel with Rust's Tokio framework"
  homepage "https://github.com/aaronriekenberg/rust-parallel"
  url "https://github.com/aaronriekenberg/rust-parallel/releases/download/v1.24.0/rust-parallel-x86_64-pc-windows-msvc.zip"
  sha256 "40ec1bc713f39f149d3391dd1fee6b0888cd2c3e18bbaf859d446733ce1e9b01"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/aaronriekenberg/rust-parallel/archive/refs/tags/v1.24.0.tar.gz"
  end

  def install
    bin.install "rust-parallel.exe"
  end

  test do
    testdata = testpath/"seq"
    testdata.write(1.upto(3).to_a.join("\n"))
    testcmd = "rust-parallel -i #{testdata} echo"
    testset = Array.new(10) { pipe_output(testcmd) }
    refute_equal testset.size, testset.uniq.size
  end
end
