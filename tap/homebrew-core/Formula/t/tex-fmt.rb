class TexFmt < Formula
  desc "Extremely fast LaTeX formatter written in Rust"
  homepage "https://wgunderwood.github.io/tex-fmt/"
  url "https://github.com/WGUNDERWOOD/tex-fmt/releases/download/v0.5.7/tex-fmt-x86_64-windows.zip"
  sha256 "807b6df5bef297eb8c44f79562bf9917b2e87f41e8e9c6b7f27e2df3f8cc9c98"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/WGUNDERWOOD/tex-fmt/archive/refs/tags/v0.5.7.tar.gz"
  end

  def install
    bin.install "tex-fmt.exe"
  end

  test do
    (testpath/"test.tex").write <<~'TEX'
      \documentclass{article}
      \title{tex-fmt Homebrew Test}
      \begin{document}
      \maketitle
      \begin{itemize}
      \item Hello
      \item World
      \end{itemize}
      \end{document}
    TEX

    assert_equal <<~'TEX', shell_output("#{bin}/tex-fmt --print #{testpath}/test.tex")
      \documentclass{article}
      \title{tex-fmt Homebrew Test}
      \begin{document}
      \maketitle
      \begin{itemize}
        \item Hello
        \item World
      \end{itemize}
      \end{document}
    TEX

    assert_match version.to_s, shell_output("#{bin}/tex-fmt --version")
  end
end
