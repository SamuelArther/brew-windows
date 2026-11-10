class Mmdbctl < Formula
  desc "MMDB file management CLI supporting various operations on MMDB database files"
  homepage "https://github.com/ipinfo/mmdbctl"
  url "https://github.com/ipinfo/mmdbctl/archive/refs/tags/mmdbctl-1.5.3.tar.gz"
  sha256 "2928e3c4c1210e04bf4069d719c81fa13eed923763957f5c3922f702fbf2d5f9"
  license "Apache-2.0"
  head "https://github.com/ipinfo/mmdbctl.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "3306edf1f49c7713222c3fa2e17c705f99ab3c4f4d80b97d1cc22a4441842f56"
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
