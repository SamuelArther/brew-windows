class Chroma < Formula
  desc "General purpose syntax highlighter in pure Go"
  homepage "https://github.com/alecthomas/chroma"
  url "https://github.com/alecthomas/chroma/archive/refs/tags/v2.27.0.tar.gz"
  sha256 "45deb53e12a92cea43e797ed3d1edde0bf6ca7492d454126506e35e8822863a1"
  license "MIT"
  head "https://github.com/alecthomas/chroma.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "79df011505614bb9443c04750e232ca73600b173f78919706835add423724495"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download", "-C", "cmd/chroma"
  end

  def install
    cd "cmd/chroma" do
      system "go", "build", *std_go_args
    end
  end

  test do
    json_output = JSON.parse(shell_output("#{bin}/chroma --json #{test_fixtures("test.diff")}"))
    assert_equal "GenericHeading", json_output[0]["type"]
  end
end
