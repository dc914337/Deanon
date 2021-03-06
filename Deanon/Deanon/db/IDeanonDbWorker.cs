﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Deanon.db.datamodels.classes.entities;
using Deanon.dumper;
using Neo4jClient;

namespace Deanon.db
{
    interface IDeanonDbWorker
    {
        Person[] GetHiddenFriendsOfUser(int id);
        void Connect();
        void ClearDatabase();
        Node<Person> AddPerson(Person user);
        void AddRelation(Person main, Person friend, EnterType type);
        Person[] GetPeopleFromMinCycles();
        Person[] GetPeopleWithoutOutRelationsAndNotDeleted();
        int[] GetAllUsersIds();
        Person[] GetAllNotDeletedPeople();
        Person[] GetUsersRelated(int userId, EnterType type);
    }
}
