class TrimGalore < Formula
  desc "Quality and adapter trimming for FastQ sequencing reads"
  homepage "https://github.com/FelixKrueger/TrimGalore"
  url "https://github.com/FelixKrueger/TrimGalore/archive/refs/tags/v2.5.0.tar.gz"
  sha256 "decc7a884b8504a216d739e8a9e26d62e855e5827bc81876bff612281f92917b"
  license "GPL-3.0-only"
  head "https://github.com/FelixKrueger/TrimGalore.git", branch: "master"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "804ede24c73d1b18da533ddd36659913f48d0eed0e98a8e9108db668bb5e4685"
  end

  depends_on "rust" => :build

  on_linux do
    depends_on "zlib-ng-compat"
  end

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/trim_galore --version")

    (testpath/"reads.fq").write "@r1\n#{"A" * 30}AGATCGGAAGAGC\n+\n#{"I" * 43}\n"
    system bin/"trim_galore", "--dont_gzip", testpath/"reads.fq"
    assert_path_exists testpath/"reads_trimmed.fq"
  end
end
