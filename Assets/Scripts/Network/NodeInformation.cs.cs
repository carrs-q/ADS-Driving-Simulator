using UnityEngine;
using System;
using System.Xml;

namespace UnityClusterPackage
{

    static class NodeInformation
    {
        private static XmlDocument xmlDocument;

        public static string type, serverIp;
        public static int id, serverPort, screen, debug;

        static NodeInformation()
        {
            xmlDocument = new XmlDocument();

            string filePath = System.IO.Path.Combine(Application.streamingAssetsPath, "node-config.xml");
            string contentXml = "";

            if (Application.platform == RuntimePlatform.Android)
            {

                WWW fileReaderAndroid = new WWW(filePath);
                while (!fileReaderAndroid.isDone) { }

                contentXml = fileReaderAndroid.text;

            }
            else
            {
                contentXml = System.IO.File.ReadAllText(filePath);
            }

            xmlDocument.LoadXml(contentXml);

            ReadNodeInformation();
        }

        static void ReadNodeInformation()
        {

            XmlNodeReader xmlNodeReader = new XmlNodeReader(xmlDocument);

            while (xmlNodeReader.Read())
            {
                if (xmlNodeReader.NodeType == XmlNodeType.Element)
                {

                    try
                    {

                        switch (xmlNodeReader.Name)
                        {

                            case "node":
                                type = xmlNodeReader.GetAttribute("type");
                                screen = Convert.ToInt32(xmlNodeReader.GetAttribute("screen"));
                                debug = Convert.ToInt32(xmlNodeReader.GetAttribute("debug"));
                                break;

                            case "server":
                                serverIp = xmlNodeReader.GetAttribute("ip");
                                serverPort = Convert.ToInt32(xmlNodeReader.GetAttribute("port"));
                                break;
                        }

                    }
                    catch (Exception ex)
                    {
                        Debug.Log("Configuration file <node-config> parsing error: " + ex);
                    }
                }
            }
        }
    }
}