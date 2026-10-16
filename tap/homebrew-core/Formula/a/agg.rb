class Agg < Formula
  desc "Asciicast to GIF converter"
  homepage "https://docs.asciinema.org/manual/agg/"
  url "https://github.com/asciinema/agg/releases/download/v1.9.0/agg-x86_64-pc-windows-msvc.exe"
  sha256 "810baf5506e74ca65d8ed85be3db58791086c8b7b0a17c9018d7fede473f0055"
  license "GPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/asciinema/agg/archive/refs/tags/v1.9.0.tar.gz"
  end

  def install
    bin.install "agg-x86_64-pc-windows-msvc.exe" => "agg.exe"
  end

  test do
    (testpath/"test.cast").write <<~EOS
      {"version": 2, "width": 80, "height": 24, "timestamp": 1504467315, "title": "Demo", "env": {"TERM": "xterm-256color", "SHELL": "/bin/zsh"}}
      [0.248848, "o", "\\u001b[1;31mHello \\u001b[32mWorld!\\u001b[0m\\n"]
      [1.001376, "o", "That was ok\\rThis is better."]
      [2.143733, "o", " "]
      [6.541828, "o", "Bye!"]
    EOS
    system bin/"agg", "--verbose", "test.cast", "test.gif"
    assert_path_exists testpath/"test.gif"
  end
end
