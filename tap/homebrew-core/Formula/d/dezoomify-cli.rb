class DezoomifyCli < Formula
  desc "Download zoomable images from museum websites, image galleries, and map viewers"
  homepage "https://dezoomify.ophir.dev"
  url "https://github.com/dezoomify/dezoomify/archive/refs/tags/26.11.24.tar.gz"
  sha256 "719bb3b2fc9d78f9e4e27cdafb59ae055e981bb2af14f9d6d6895b1a4d7bcadf"
  license "GPL-3.0-or-later"
  head "https://github.com/dezoomify/dezoomify.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "a5ab7bca92781b67d110ea2f59071ba665592ebe04975a6c476d9217536fc155"
  end

  depends_on "node" => :build
  depends_on "pnpm" => :build
  depends_on "rust" => :build

  on_intel do
    depends_on "nasm" => :build
  end

  allow_network_access! :test

  def fetch
    system "pnpm", "install", "--frozen-lockfile", "--ignore-scripts"
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args(path: "apps/cli")
  end

  test do
    (testpath/"tiles.yaml").write <<~YAML
      url_template: "https://cdn.jsdelivr.net/gh/lovasoa/dezoomify-rs@v2.11.2/testdata/generic/map_{{x}}_{{y}}.jpg"
      x_template: "x * tile_size"
      y_template: "y * tile_size"
      variables:
        - { name: x, from: 0, to: 1 } # Image width, in tiles
        - { name: y, from: 0, to: 1 } # Image height, in tiles
        - { name: tile_size, value: 256 }
      title: "testtile"
    YAML
    dezoom_out = shell_output("#{bin}/dezoomify-cli tiles.yaml testtile.png 2>&1")
    assert_match "saved testtile.png", dezoom_out
    png = (testpath/"testtile.png").binread(24)
    assert_equal "\x89PNG\r\n\x1A\n\0\0\0\rIHDR".b, png[0, 16]
    assert_equal [512, 512], png[16, 8].unpack("NN")
  end
end
