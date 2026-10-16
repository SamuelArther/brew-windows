class Diagram < Formula
  desc "CLI app to convert ASCII arts into hand drawn diagrams"
  homepage "https://github.com/esimov/diagram"
  url "https://github.com/esimov/diagram/archive/refs/tags/v1.1.0.tar.gz"
  sha256 "f88bc99975ade753435ecf0e7a6470611f77563eb73b94d56fa6b6bafb4b8561"
  license "MIT"
  head "https://github.com/esimov/diagram.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "8b3a042841c48ea79a0377fb136dc2b27436d9165aad1c4b22ab12bd1cc97886"
  end

  depends_on "go" => :build
  depends_on "pkgconf" => :build

  on_linux do
    depends_on "vulkan-headers" => :build
    depends_on "libx11"
    depends_on "libxcursor"
    depends_on "libxfixes"
    depends_on "libxkbcommon"
    depends_on "mesa"
    depends_on "wayland"
  end

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    # Workaround to avoid patchelf corruption when cgo is required (for gioui.org/internal/vk)
    if OS.linux? && Hardware::CPU.arch == :arm64
      ENV["CGO_ENABLED"] = "1"
      ENV["GO_EXTLINK_ENABLED"] = "1"
      ENV.append "GOFLAGS", "-buildmode=pie"
    end

    system "go", "build", *std_go_args(ldflags: "-X main.defaultFontFile=#{pkgshare}/gloriahallelujah.ttf")

    pkgshare.install ["sample.txt", "font/gloriahallelujah.ttf"]
  end

  test do
    cp pkgshare/"sample.txt", testpath
    pid = spawn bin/"diagram", "-in", "sample.txt", "-out", testpath/"output.png"
    sleep 1
    assert_path_exists testpath/"output.png"
  ensure
    Process.kill("TERM", pid)
    Process.wait(pid)
  end
end
