class Html2text < Formula
  desc "Advanced HTML-to-text converter"
  homepage "https://gitlab.com/grobian/html2text"
  url "https://gitlab.com/-/project/48313341/uploads/8526650dd42218b3493ce7ca0a3eeb1e/html2text-2.4.0.tar.gz"
  sha256 "9d0a7174cacbb3f050b60facd8cba6e138944ec5020b16d1cee70cf91a59f132"
  license "GPL-2.0-or-later"
  head "https://gitlab.com/grobian/html2text.git", branch: "master"

  deny_network_access!

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "3d43a3b697367f0273f052e06d165338c3390b3bee4900b7f1f92017af67a292"
  end

  def install
    # libiconv is not linked properly without this
    ENV.append "LDFLAGS", "-liconv" if OS.mac?

    ENV.cxx11
    system "./configure", "--disable-silent-rules", *std_configure_args
    system "make", "install"
  end

  test do
    path = testpath/"index.html"
    path.write <<~HTML
      <!DOCTYPE html>
      <html>
        <head><title>Home</title></head>
        <body><p>Hello World</p></body>
      </html>
    HTML

    assert_equal "Hello World", shell_output("#{bin}/html2text #{path}").strip
  end
end
