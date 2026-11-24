class Convox < Formula
  desc "Command-line interface for the Convox PaaS"
  homepage "https://convox.com/"
  url "https://github.com/convox/convox/archive/refs/tags/3.26.1.tar.gz"
  sha256 "429b9ba0b119273d374c4ce200b36a1e5889d927dbc847250bb412bc908fcabb"
  license "Apache-2.0"
  version_scheme 1
  head "https://github.com/convox/convox.git", branch: "master"

  livecheck do
    url :stable
    strategy :github_latest
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "5b0d40219588709c2a3c160eecf04da19eafcae8701ee2b82c0adec1e901143e"
  end

  depends_on "go" => :build
  depends_on "pkgconf" => :build

  on_linux do
    depends_on "systemd" # for libudev
  end

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = "-X main.version=#{version}"
    system "go", "build", "-mod=readonly", *std_go_args(ldflags:), "./cmd/convox"
  end

  test do
    assert_equal "Authenticating with localhost... ERROR: invalid login\n",
      shell_output("#{bin}/convox login -t invalid localhost 2>&1", 1)
  end
end
