class TrimGalore < Formula
  desc "Quality and adapter trimming for FastQ sequencing reads"
  homepage "https://github.com/FelixKrueger/TrimGalore"
  url "https://github.com/FelixKrueger/TrimGalore/archive/refs/tags/v2.3.0.tar.gz"
  sha256 "6dd4d99c10a2d1e740d8e49ee870b1f9a7ccb8ee94c0bc5183d2ae507492aab0"
  license "GPL-3.0-only"
  head "https://github.com/FelixKrueger/TrimGalore.git", branch: "master"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "4a96eb08e5e0de6a20868f3350027b638c70a51be2b81cfb04990eed13d9c9b3"
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
