class Wego < Formula
  desc "Weather app for the terminal"
  homepage "https://github.com/schachmat/wego"
  url "https://github.com/schachmat/wego/archive/refs/tags/2.4.tar.gz"
  sha256 "5ae1c76a322ff2e295034a801755c42b0cb6bdef5ab205af42cf95dbf7c570f4"
  license "ISC"
  head "https://github.com/schachmat/wego.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "6b7a2967be609afb25ae967f8760642ccdb42ff5a06e4ffbbc6621974d3fcadc"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args
  end

  test do
    ENV["WEGORC"] = testpath/".wegorc"
    assert_match(/No .*API key specified./, shell_output("#{bin}/wego 2>&1", 1))
  end
end
