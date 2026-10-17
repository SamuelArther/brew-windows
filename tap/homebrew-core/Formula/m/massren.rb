class Massren < Formula
  desc "Easily rename multiple files using your text editor"
  homepage "https://github.com/laurent22/massren"
  url "https://github.com/laurent22/massren/archive/refs/tags/v1.5.7.tar.gz"
  sha256 "7e7dd149bd3364235247268cc684b5a35badd9bee22f39960e075c792d037a8c"
  license "MIT"
  head "https://github.com/laurent22/massren.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "b883ae5339e70ff6c956b5a24741cde9ce5560d8dc691c2967fb623c280c301d"
  end

  depends_on "go" => :build

  deny_network_access!

  def install
    ENV["CGO_ENABLED"] = "1" if OS.linux? && Hardware::CPU.arm?

    system "go", "build", *std_go_args
  end

  test do
    system bin/"massren", "--config", "editor", "nano"
    assert_match 'editor = "nano"', shell_output("#{bin}/massren --config")
  end
end
