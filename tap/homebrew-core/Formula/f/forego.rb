class Forego < Formula
  desc "Foreman in Go for Procfile-based application management"
  homepage "https://github.com/ddollar/forego"
  license "Apache-2.0"
  head "https://github.com/ddollar/forego.git", branch: "master"

  stable do
    url "https://github.com/ddollar/forego/archive/refs/tags/20180216151118.tar.gz"
    sha256 "23119550cc0e45191495823aebe28b42291db6de89932442326340042359b43d"

    # Add go.mod
    patch do
      url "https://github.com/ddollar/forego/commit/89fb456a167f59ace41e0e9294f4b7c01f76943e.patch?full_index=1"
      sha256 "98274160eb0251af323df030f8f05369ab19b0116a2b87e58372974bae5c0524"
      type :backport
    end
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "1b8aa456b43c36ccec4a91a46272e87780d26b5bc95f3a0939dd34da8cfc0768"
  end

  depends_on "go" => :build

  deny_network_access!

  def install
    ldflags = "-X main.Version=#{version} -X main.allowUpdate=false"
    system "go", "build", *std_go_args(ldflags:)
  end

  test do
    (testpath/"Procfile").write "web: echo 'it works!'"
    assert_match "it works", shell_output("#{bin}/forego start")
  end
end
