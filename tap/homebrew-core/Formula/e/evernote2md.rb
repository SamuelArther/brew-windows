class Evernote2md < Formula
  desc "Convert Evernote .enex file to Markdown"
  homepage "https://github.com/wormi4ok/evernote2md"
  url "https://github.com/wormi4ok/evernote2md/releases/download/v0.23.0/evernote2md_0.23.0_Windows_amd64.zip"
  sha256 "82cd88b13b6986f1c729fc7625d950b61ba24ce77f39f30574760ccb853c888f"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/wormi4ok/evernote2md/archive/refs/tags/v0.23.0.tar.gz"
  end

  def install
    bin.install "evernote2md.exe"
  end

  test do
    (testpath/"export.enex").write <<~XML
      <?xml version="1.0" encoding="UTF-8"?>
      <!DOCTYPE en-export SYSTEM "http://xml.evernote.com/pub/evernote-export3.dtd">
      <en-export>
        <note>
          <title>Test</title>
          <content>
            <![CDATA[<?xml version="1.0" encoding="UTF-8" standalone="no"?>
      <!DOCTYPE en-note SYSTEM "http://xml.evernote.com/pub/enml2.dtd"><en-note><div><br /></div></en-note>]]>
          </content>
        </note>
      </en-export>
    XML
    system bin/"evernote2md", "export.enex"
    assert_path_exists testpath/"notes/Test.md"
  end
end
