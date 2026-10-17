class Spoa < Formula
  desc "SIMD partial order alignment tool/library"
  homepage "https://github.com/rvaser/spoa"
  url "https://github.com/rvaser/spoa/archive/refs/tags/4.1.5.tar.gz"
  sha256 "b5d323740b01255c55725e88db2548c666a05bc83b825c19cfac10586e21e7b3"
  license "MIT"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "90d58b542961931277e6ef9d92cddc8cdefb33145b457cd1da869fa8f88f52d8"
  end

  depends_on "cmake" => :build

  on_linux do
    depends_on "zlib-ng-compat"
  end

  def install
    args = %w[
      -DBUILD_SHARED_LIBS=ON
      -Dspoa_build_tests=OFF
    ]
    args << "-DCMAKE_INSTALL_RPATH=#{rpath}" if OS.mac?
    system "cmake", "-S", ".", "-B", "build", *args, *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
    pkgshare.install "test/data"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/spoa --version")

    output = shell_output("#{bin}/spoa #{pkgshare}/data/sample.fastq.gz")
    assert_match ">Consensus", output
  end
end
