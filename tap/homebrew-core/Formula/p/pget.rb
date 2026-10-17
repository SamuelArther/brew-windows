class Pget < Formula
  desc "File download client"
  homepage "https://github.com/Code-Hex/pget"
  url "https://github.com/Code-Hex/pget/archive/refs/tags/v0.2.1.tar.gz"
  sha256 "fa7646bec975dd3995fb45d6b1f190565d6c4fae03c46c4eda34716c83ede03e"
  license "MIT"
  head "https://github.com/Code-Hex/pget.git", branch: "master"

  deprecate! date: "2026-08-23", because: :unmaintained
  disable! date: "2027-08-23", because: :unmaintained

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "e6137cd605fb36c9feec3d3f04017f8cf94afa10d620fa1861aef22998b4fbd0"
  end

  depends_on "go" => :build

  # Failing on Intel macOS with Go 1.27 due to outdated golang.org/x/sys dependency
  on_macos do
    depends_on arch: :arm64
  end

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = "-s -w -X main.version=#{version}"
    system "go", "build", *std_go_args(ldflags:), "./cmd/pget"
  end

  test do
    file = "https://raw.githubusercontent.com/Homebrew/homebrew-core/HEAD/README.md"
    system bin/"pget", "-p", "4", file
    assert_path_exists testpath/"README.md"

    assert_match version.to_s, shell_output("#{bin}/pget --help", 1)
  end
end
