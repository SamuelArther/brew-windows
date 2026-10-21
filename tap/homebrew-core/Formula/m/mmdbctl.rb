class Mmdbctl < Formula
  desc "MMDB file management CLI supporting various operations on MMDB database files"
  homepage "https://github.com/ipinfo/mmdbctl"
  url "https://github.com/ipinfo/mmdbctl/archive/refs/tags/mmdbctl-1.5.1.tar.gz"
  sha256 "784f4b5a5517cae63a07d964a79551dcb9edf2bee6cf11952a11f57fa3378ee2"
  license "Apache-2.0"
  head "https://github.com/ipinfo/mmdbctl.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "a4ad7ce737b3393efc8af7729271feb48d2855bfc8055c2c627ebbe9300b82cb"
  end

  depends_on "go" => :build

  resource "test.mmdb", :test do
    url "https://raw.githubusercontent.com/maxmind/MaxMind-DB/02de12f89048db626d04f8865c6fc76eac9a7a6b/test-data/GeoIP2-City-Test.mmdb"
    sha256 "df1eb8e048d3b2561f477cd27f7d642fc25a24767395071d782ae927036818a0"
  end

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args

    generate_completions_from_executable(bin/"mmdbctl", shell_parameter_format: :cobra)
  end

  test do
    testpath.install resource("test.mmdb")

    system bin/"mmdbctl", "verify", testpath/"GeoIP2-City-Test.mmdb"

    output = shell_output("#{bin}/mmdbctl metadata #{testpath}/GeoIP2-City-Test.mmdb")
    assert_match "GeoIP2 City Test Database (fake GeoIP2 data, for example purposes only)", output
  end
end
