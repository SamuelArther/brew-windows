class Graphviz < Formula
  desc "Graph visualization software from AT&T and Bell Labs"
  homepage "https://graphviz.org/"
  version "16.1.0"
  url "https://gitlab.com/api/v4/projects/4207231/packages/generic/graphviz-releases/16.1.0/windows_10_cmake_Release_Graphviz-16.1.0-win64.zip"
  sha256 "733e49626c492242eb8dca30ea627b6ead20710e207998c7933b4909d92d6abc"
  license "EPL-1.0"

  def install
    install_tree
    %w[acyclic bcomps ccomps circo dijkstra dot fdp gc gvcolor gvgen gvpack gvpr neato nop osage
       patchwork sccmap sfdp tred twopi unflatten].each do |cmd|
      windows_exe "libexec/bin/#{cmd}.exe", cmd
    end
  end

  test do
    (testpath/"sample.dot").write <<~EOS
      digraph G {
        a -> b
      }
    EOS

    system bin/"dot", "-Tpdf", "-o", "sample.pdf", "sample.dot"
  end
end
